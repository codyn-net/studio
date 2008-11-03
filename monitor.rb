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
			@coloridx = 0
			@linkrulers = true

			build
			
			signal_register(Simulation.instance, 'step') { |s| update(s.timestep) }
			signal_register(Simulation.instance, 'period_start') { |s, from, timestep, to| do_period_start(from, timestep, to) }
			signal_register(Simulation.instance, 'period_stop') { |s| do_period_stop }
		
			set_default_size(500, 400)
		end
		
		def content_area
			@content = Gtk::VBox.new(false, 6)
		end
		
		def build
			hbox = Gtk::HBox.new(false, 3)
			but = Gtk::ToggleButton.new
			but.relief = Gtk::RELIEF_NONE
			
			im1 = Gtk::Image.new(Stock.icon_path('chain-broken.png'))
			im2 = Gtk::Image.new(Stock.icon_path('chain.png'))
			
			hbox.pack_end(but, false, false, 0)
			
			but.signal_connect('toggled') do |x| 
				@linkrulers = x.active?
				but.image = x.active? ? im2 : im1
			end
			
			but.active = @linkrulers

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
		end
		
		def remove_hook_real(obj, container)
			super
			
			Simulation.instance.unset_monitor(obj, container[:prop])
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
			exp = Gtk::Expander.new(property_name(obj, prop, true))
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
			
			# register monitoring
			Simulation.instance.set_monitor(obj, prop)

			super(obj, prop, state.merge({:graph => g, :widget => hbox, :expander => exp}))
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
				title = property_name(obj, a[:prop], true)
				a[:expander].label = title unless a[:expander].label == title
			end
		end
		
		def do_period_start(from, timestep, to)
			@inperiod = true
			@range = [from, timestep, to]
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
				rstep = (@range[2] - @range[0]) / numpix.to_f;
				dpx = 2
				
				to = (0...(numpix / dpx)).collect { |x| @range[0] + (x * rstep * dpx) }
				
				data = Simulation.instance.monitor_data_resampled(obj, v[:prop], to)
				data.collect! { |x| (x.nan? || x.infinite?) ? 0 : x } 
				
				dist = (data.max - data.min) / 2.0
				v[:graph].yaxis = [data.min - dist * 0.2, data.max + dist * 0.2]
				v[:graph].sample_frequency = 1
				v[:graph].unit_width = dpx
				v[:graph].data = data
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
			
			return if Simulation.instance.period?

			@map.each do |obj, p|
				c = Simulation.instance.setup_context(obj)

				p.each do |v|
					v[:graph] << Simulation.instance.monitor_data(obj, v[:prop])[-1]
				end
			end
		end
	end
end
