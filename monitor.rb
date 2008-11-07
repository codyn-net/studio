require 'graph'
require 'stock'
require 'simulation'
require 'hook'
require 'table'

module Cpg
	class Monitor < Hook
		type_register

		SAMPLE_WIDTH = 2
		
		attr_reader :content
		
		class Container < Gtk::EventBox
			attr_reader :frame, :graph, :close, :merge
	
			def initialize(g)
				super()
				
				hbox = Gtk::HBox.new(false, 0)
				self << hbox
				@graph = g
				
				@frame = Gtk::Frame.new
				@frame.shadow_type = Gtk::ShadowType::ETCHED_IN
				@frame << g
			
				# FIXME: no scrollbars for now
				#vs.pack_start(Gtk::HScrollbar.new(g.adjustment), false, false, 0)

				hbox.pack_start(@frame, true, true, 0)
			
				@close = Stock.close_button		
				
				vbox = Gtk::VBox.new(false, 3)
				vbox.pack_start(@close, false, false, 0)
			
				@merge = Stock.small_button(Gtk::Stock::CONVERT)
				vbox.pack_start(@merge, false, false, 0)
		
				hbox.pack_start(vbox, false, false, 0)
			end
			
			def create_drag_icon
				a = @frame.allocation
				
				pix = Gdk::Pixbuf.from_drawable(nil, @frame.window, 0, 0, a.width, a.height)
				pix.scale(a.width * 0.7, a.height * 0.7, Gdk::Pixbuf::INTERP_HYPER)
			end
		end
	
		def initialize(dt)
			super({:title => 'Monitor'})
		
			@dt = dt
			@coloridx = 0
			@linkrulers = true
			@linkaxis = true

			build
			
			signal_register(Simulation.instance, 'step') { |s| update(s.timestep) }
			signal_register(Simulation.instance, 'period_start') { |s, from, timestep, to| do_period_start(from, timestep, to) }
			signal_register(Simulation.instance, 'period_stop') { |s| do_period_stop }
		
			set_default_size(500, 400)
			
			ag = Gtk::ActionGroup.new('MonitorActions')
			
			ag.add_actions([
				['AutoAxisAction', nil, 'Auto scale axis', '<Control>r', 'Automatically scale axis for data', Proc.new { |g, a| do_auto_axis }]
			])
			
			ag.add_toggle_actions([
				['LinkedAxisAction', nil, 'Link axis scales', '<Control>l', 'Link graph axis scales', Proc.new { |g, a| do_link_axis(a.active?) }, @linkaxis]
			])
			
			@uimanager.insert_action_group(ag, 0)
			
			mid = @uimanager.new_merge_id
			@uimanager.add_ui(mid, "/menubar/View/ViewBottom", "AutoAxis", "AutoAxisAction", Gtk::UIManager::MENUITEM, false)
			@uimanager.add_ui(mid, "/menubar/View/ViewBottom", "LinkedAxis", "LinkedAxisAction", Gtk::UIManager::MENUITEM, false)  
		end
		
		def set_size(cols, rows)
			cols = cols.to_i
			rows = rows.to_i
			
			cols = @content.n_columns if cols <= 0
			rows = @content.n_rows if rows <= 0
			
			@content.resize(rows, cols)
		end
		
		def size
			return [@content.n_columns, @content.n_rows]
		end
		
		def content_area
			@content = Table.new(1, 1, true)
			@content.expand = Table::EXPAND_DOWN
			@content.row_spacings = 1
			@content.column_spacings = 1
			
			@content
		end
		
		def do_link_axis(active)
			@linkaxis = active
			
			if @linkaxis
				yax = [nil, nil]
				
				each_graph do |obj, container|
					y = container[:graph].yaxis
					
					yax[0] = y[0] if (yax[0] == nil || y[0] < yax[0])
					yax[1] = y[1] if (yax[1] == nil || y[1] > yax[1])
				end
				
				return unless yax[0] && yax[1]
				
				each_graph do |obj, container|
					container[:graph].yaxis = yax
				end
			end
		end
		
		def do_auto_axis
			each_graph do |obj, container|
				container[:graph].auto_axis
			end
			
			do_link_axis(@linkaxis)
		end
		
		def build
			hbox = Gtk::HBox.new(false, 6)
			
			but = Stock.chain_button do |x| 
				@linkrulers = x.active?
			end
			
			but.active = @linkrulers
			
			hbox.pack_end(but, false, false, 0)

			@showrulers = Gtk::CheckButton.new('Show graph rulers')
			@showrulers.active = true
			hbox.pack_end(@showrulers, false, false, 0)
						
			@vbox_content.pack_end(hbox, false, false, 3)
			
			@showrulers.signal_connect('toggled') do |t|
				each_graph do |o, x|
					x[:graph].show_ruler = t.active?
				end
			end
			
			hbox.show_all
		end
		
		def install_object(obj, prop)
			super
			
			signal_register(obj, 'property_changed') do |o, p| 
				if ['display', 'label', 'equation', 'id'].include?(p)
					update_title(o, prop)
				end
			end
		end
		
		def remove_hook_real(obj, container)
			g = container[:graph]
			
			g.signal_handler_disconnect(container[:configure]) if container[:configure]

			if g.num_plots > 1
				# prevent removal of graph
				g.remove(container[:plot])
				container[:widget] = nil
			end
			
			super
			
			Simulation.instance.unset_monitor(obj, container[:prop])
		end
		
		def set_yaxis(obj, prop, yax)
			@map[obj].each do |p|
				if p[:prop] == prop
					p[:graph].yaxis = yax
					return
				end
			end
		end
		
		def yaxis(obj, prop)
			@map[obj].each do |p|
				if p[:prop] == prop
					return p[:graph].yaxis
				end
			end
			
			return [-3, 3]
		end
	
		def add_hook_real(obj, prop, state)
			@map[obj].each do |p|
				return if p[:prop] == prop
			end

			g = Graph.new(SAMPLE_WIDTH, [-3, 3])
			g.set_size_request(-1, 50)
			g.show_ruler = @showrulers.active?
			
			if @linkaxis && !@map.empty? && !@map[@map.keys[0]].empty?
				g.yaxis = @map[@map.keys[0]][0][:graph].yaxis
			end
			
			cont = Container.new(g)
			
			cont.close.signal_connect('clicked') { |x| remove_all_hooks(obj, prop) }
			cont.merge.signal_connect('clicked') { |x| show_merge_menu(obj, prop) }
			cont.show_all

			@content << cont
			
			g.signal_connect_after('motion_notify_event') do |o,ev|
				do_linkrulers(g, ev) if @linkrulers && g.show_ruler
			end
			
			g.signal_connect_after('leave_notify_event') do |o,ev|
				do_linkrulers_leave(g, ev) if @linkrulers && g.show_ruler
			end
			
			# register monitoring
			Simulation.instance.set_monitor(obj, prop)

			container = super(obj, prop, state.merge({:graph => g, :widget => cont, :plot => (g << [])}))
			
			container[:plot].label = property_name(obj, prop, true)
			
			# request resimulate
			Simulation.instance.resimulate
		end
		
		def monitor_position(obj, prop)
			h = find_hook(obj, prop)
			@content.get_position(h[:widget])
		end
		
		def show_merge_menu(obj, prop)
			h = find_hook(obj, prop)
			menu = Gtk::Menu.new
			
			pos = @content.get_position(h[:widget])
			
			spec = [
				[Gtk::Stock::GOTO_TOP, 'Merge up', 0, -1],
				[Gtk::Stock::GOTO_BOTTOM, 'Merge Down', 0, 1],
				[Gtk::Stock::GOTO_LAST, 'Merge right', 1, 0],
				[Gtk::Stock::GOTO_FIRST, 'Merge left', -1, 0]
			]
			
			spec.each do |s|
				next if pos[0] + s[2] < 0 || pos[0] + s[2] >= @content.n_columns ||
					    pos[1] + s[3] < 0 || pos[1] + s[3] >= @content.n_rows

				item = Gtk::ImageMenuItem.new(s[1])
				item.image = Gtk::Image.new(s[0], Gtk::IconSize::MENU)
				
				item.signal_connect('activate') do |i|
					do_merge(obj, prop, s[2], s[3])
				end
				
				menu << item
				
				item.show
			end
			
			if !menu.children.empty?			
				menu.popup(nil, nil, 1, 0)
			end
		end
		
		def remove_all_hooks(obj, prop)
			h = find_hook(obj, prop)
			
			return unless h
			widget = h[:widget]
			
			items = find_for_widget(widget)
			
			items.each do |x|
				remove_hook(x[0], x[1][:prop])
			end
		end
		
		def find_for_widget(w)
			res = []
			
			@map.each do |obj, lst|
				lst.each { |x| res << [obj, x] if x[:widget] == w }
			end
			
			res
		end
		
		def set_monitor_position(obj, prop, x, y)
			h = find_hook(obj, prop)
			w = @content.at(x, y)
			
			if w && w != @content.child_real(h[:widget])
				merge_with(obj, prop, w)
			else
				@content.child_position(h[:widget], x, y)
			end
		end
		
		def merge_with(obj, prop, to)
			h = find_hook(obj, prop)
			
			return unless h
			widget = h[:widget]
			
			items = find_for_widget(widget)
			toitem = find_for_widget(to).first[1]
			
			# merge items with the found graph
			items.each do |item|
				o = item[0]
				item = item[1]
				
				item[:plot] = toitem[:graph].add(item[:plot].data, item[:plot].color, item[:plot].label)
				item[:graph] = toitem[:graph]
				item[:widget] = to
				
				GLib::Source.remove(item[:configure]) if item[:configure]
				item[:configure] = nil
			end
			
			update_title(obj, prop)
			
			widget.destroy
		end
		
		def do_merge(obj, prop, dx, dy)
			h = find_hook(obj, prop)
			
			return unless h

			to = @content.find(h[:widget], dx, dy)
			
			return unless to
			
			merge_with(obj, prop, to)
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
			h = find_hook(obj, prop)
			
			return unless h
			
			h[:plot].label = property_name(obj, prop, true)
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
		
		def set_monitor_data(obj, v)
			numpix = v[:graph].allocation.width
				
			# resample data to be on pxd
			rstep = (@range[2] - @range[0]) / numpix.to_f;
			
			to = (0...(numpix / SAMPLE_WIDTH)).collect { |x| @range[0] + (x * rstep * SAMPLE_WIDTH) }
			
			data = Simulation.instance.monitor_data_resampled(obj, v[:prop], to)
			data.collect! { |x| (x.to_f.nan? || x.to_f.infinite?) ? 0.0 : x.to_f } 

			v[:plot].data = data || []
		end
		
		def update_monitor_data(obj, v)
			# find all plots
			items = find_for_widget(v[:widget])
			
			items.each do |x|
				set_monitor_data(x[0], x[1])
			end
		end
		
		def set_configure_handler(obj, v)
			if !v[:configure]				
				v[:configure] = v[:graph].signal_connect('configure-event') do |g, ev|
					GLib::Source.remove(v[:configure_source]) if v[:configure_source]
					
					v[:configure_source] = GLib::Timeout.add(50) do
						v[:configure_source] = nil
						update_monitor_data(obj, v)
						false
					end
				end
			end
		end
		
		def do_period_stop
			# configure graphs so that they show the collected data correctly
			configures = {}
			
			each_graph do |obj, v|
				set_monitor_data(obj, v)
				
				g = v[:graph]
				set_configure_handler(obj, v) unless configures[g]
				configures[g] = true
			end
		end
		
		def reset_graphs
			each_graph do |obj, v|
				v[:plot].data = []
				v[:graph].yaxis = [-3, 3]
			end
		end
	
		def update(timestep)
#			if @inperiod && !Simulation.instance.period?
#				reset_graphs
#				@inperiod = false
#			end
#			
#			return if Simulation.instance.period?

#			@map.each do |obj, p|
#				c = Simulation.instance.setup_context(obj)

#				p.each do |v|
#					GLib::Source.remove(v[:configure_source]) if v[:configure_source]
#					v[:graph].signal_handler_disconnect(v[:configure])
#					v.delete(:configure)
#					v.delete(:configure_source)
#					
#					v[:graph] << Simulation.instance.monitor_data(obj, v[:prop])[-1]
#				end
#			end
		end
	end
end
