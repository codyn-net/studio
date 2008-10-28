require 'graph'
require 'stock'
require 'simulation'
require 'hook'

module Cpg
	class Monitor < Hook
		type_register
	
		COLORS = [
			[0, 0, 0.6],
			[0, 0.6, 0],
			[0.6, 0, 0],
			[0, 0.6, 0.6],
			[0.6, 0.6, 0],
			[0.6, 0, 0.6],
			[0, 0, 0]
		]
		
		DEFAULT_UNIT_WIDTH = 40
	
		def initialize(dt)
			super({:title => 'Monitor'})
		
			@dt = dt
			@grid = Application.instance.grid
			@coloridx = 0
			@linkrulers = false
			@adjustsignals = {}

			build
			
			signal_register(Simulation.instance, 'step') { |s| update(s.timestep) }
			signal_register(Simulation.instance, 'period_start') { |s, period| do_period_start(period) }
			signal_register(Simulation.instance, 'period_stop') { |s| do_period_stop }
		
			set_default_size(500, 400)
		end
		
		def content_area
			@content = Gtk::VBox.new(false, 6)
		end
		
		def build
			hbox = Gtk::HBox.new(false, 3)
			but = Gtk::ToggleButton.new
			but.active = false
			but.relief = Gtk::RELIEF_NONE
			
			im1 = Gtk::Image.new(Stock.icon_path('chain-broken.png'))
			im2 = Gtk::Image.new(Stock.icon_path('chain.png'))
			
			but.image = im1
			hbox.pack_end(but, false, false, 0)
			
			but.signal_connect('toggled') do |x| 
				@linkrulers = x.active?
				but.image = x.active? ? im2 : im1
			end

			@showrulers = Gtk::CheckButton.new('Show graph rulers')
			@showrulers.active = true
			hbox.pack_end(@showrulers, false, false, 0)
						
			@vbox_content.pack_end(hbox, false, false, 0)
			
			@showrulers.signal_connect('toggled') do |t|
				each_graph do |o, x|
					x[:graph].show_ruler = t.active?
				end
			end
			
			hbox.show_all
		end
	
		def next_color
			c = COLORS[@coloridx]
		
			@coloridx += 1
			@coloridx = 0 if @coloridx == COLORS.length
		
			return c
		end
		
		def install_object(obj, prop)
			super
			
			signal_register(obj, 'property_changed') { |o, p| update_title(o, p) }
			
			@adjustsignals[obj] = {}
		end
	
		def add_hook_real(obj, prop, state)
			@map[obj].each do |p|
				return if p[:prop] == prop
			end

			g = Graph.new(1 / @dt, DEFAULT_UNIT_WIDTH, [-3, 3])
			g.color = next_color
			g.set_size_request(-1, 50)
			g.show_ruler = @showrulers.active?
			
			hbox = Gtk::HBox.new(false, 3)
			exp = Gtk::Expander.new(property_name(obj, prop))
			vs = Gtk::VBox.new(false, 3)
			vs.pack_start(g, true, true, 0)
			vs.pack_start(Gtk::HScrollbar.new(g.adjustment), false, false, 0)
		
			exp.add(vs)
			exp.expanded = true
		
			hbox.pack_start(exp, true, true, 0)
	
			close = Gtk::Button.new
			close.image = Gtk::Image.new(Gtk::Stock::CLOSE, Gtk::IconSize::MENU)
			close.relief = Gtk::ReliefStyle::NONE
		
			close.signal_connect('clicked') { |x| remove_hook(obj, prop) }
		
			align = Gtk::VBox.new(false, 0)
			align.pack_start(close, false, false, 0)
		
			hbox.pack_start(align, false, false, 0)
			hbox.show_all

			@content.pack_start(hbox, true, true, 0)
		
			exp.signal_connect('activate') do |o|
				active = !exp.expanded?
			
				@content.set_child_packing(hbox, active, active, 0, Gtk::PackType::START)
			end
			
			g.signal_connect_after('motion_notify_event') do |o,ev|
				do_linkrulers(g, ev) if @linkrulers && g.show_ruler
			end
			
			g.signal_connect_after('leave_notify_event') do |o,ev|
				do_linkrulers_leave(g, ev) if @linkrulers && g.show_ruler
			end
			
			#@adjustsignals[obj][prop.to_sym] = g.adjustment.signal_connect('value_changed') do |o|#
			#	linked_adjustments(g) if @linkrulers
			#end
		
			super(obj, prop, state.merge({:graph => g, :widget => hbox, :expander => exp}))
		end
		
		def linked_adjustments(g)
		
		end
		
		def signals_unregister(obj)
			super

			#if @adjustsignals.include?(obj)
			#	@adjustsignals[obj].values.each { |x| obj.signal_handler_disconnect(x) }
			#	@adjustsignals.delete(obj)
			#end
		end
		
		def do_linkrulers(g, ev)
			# propagate ruler position to other graphs
			each_graph do |obj, x|
				x[:graph].ruler = g.ruler if x[:graph] != g
			end
			
			false
		end
		
		def do_linkrulers_leave(g, ev)
			# remove rulers
			each_graph do |obj, x|
				x[:graph].ruler = nil
			end
			
			false
		end
		
		def update_title(obj, prop)
			@map[obj].each do |a|
				title = property_name(obj, a[:prop])
				a[:expander].label = title unless a[:expander].label == title
			end
		end
		
		def do_period_start(steps)
			# initialize period_data container
			each_graph do |obj, v|
				v[:period_data] = []		
				#obj.signal_handler_block(@adjustsignals[obj][v[:prop]])
			end
			
			@inperiod = true
			@steps = steps
		end
		
		def each_graph
			@map.each do |obj, p|
				p.each do |v|
					yield obj, v
				end
			end
		end
		
		def do_period_stop
			# configure graphs so that they show the collected data correctly
			each_graph do |obj, v|
				numpix = v[:graph].allocation.width
				
				# resample data to be on pxd
				sites = (1..@steps).to_a.collect { |x| x * (numpix / @steps.to_f) }
				to = (1...numpix)
				
				v[:period_data].collect! { |x| (x.nan? || x.infinite?) ? 0 : x } 
				
				data = v[:period_data].resample(sites, to)
				
				v[:graph].yaxis = [data.min * 1.2, data.max * 1.2]
				v[:graph].sample_frequency = 1
				v[:graph].unit_width = 1
				v[:graph].data = data
				
				#obj.signal_handler_unblock(@adjustsignals[obj][v[:prop]])
			end
		end
		
		def reset_graphs
			each_graph do |obj, v|
				v[:graph].data = []
				v[:graph].unit_width = DEFAULT_UNIT_WIDTH
				v[:graph].sample_frequency = (1 / @dt).to_i
				v[:graph].yaxis = [-3, 3]
			end
		end
	
		def update(timestep)
			if @inperiod && !Simulation.instance.period?
				reset_graphs
				@inperiod = false
			end
			
			@map.each do |obj, p|
				c = MathContext.new(Simulation.instance.state, obj.state)

				p.each do |v|
					#obj.signal_handler_block(@adjustsignals[obj][v[:prop]])

					val = c.eval(obj.get_property(v[:prop])).to_f
					
					if Simulation.instance.period?
						v[:period_data] << val
					else
						v[:graph] << val
					end

					#obj.signal_handler_unblock(@adjustsignals[obj][v[:prop]])
				end
			end
		end
	end
end
