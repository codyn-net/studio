require 'ccpg'
require 'flatformat'

module Cpg
	class Simulation
		def initialize(root, timestep)
			super({})

			self.root = root
		
			@timestep = timestep
			@monitors = {}
			@range = nil
			reset
			
			@handlemodified = false
			@map = {}
		end
		
		def do_object_added(obj)
			# make sure to connect property changes
			obj.signal_connect('property_changed') do |obj, prop|
				if @map.include?(obj)
					@handlemodified = false
					
					# set property value we can do
					@map[obj].set_value(prop, obj.get_property(prop.to_sym).to_s)
				end
			end
			
			obj.signal_connect('property_added') do |obj, prop|
				if @map.include?(obj)
					@handlemodified = false
					
					# adding a new property we can do
					@map[obj].add_property(prop, obj.is_a?(Components::SimulatedObject) ? obj.initial_value(prop.to_sym).to_s : obj.get_property(prop.to_sym).to_s, obj.is_a?(Components::SimulatedObject) ? obj.integrated?(prop.to_sym) : false)
				end
			end
			
			if obj.is_a?(Components::SimulatedObject)
				obj.signal_connect('initial_changed') do |obj, prop|
					if @map.include?(obj)
						@handlemodified = false
					
						# handle initial changed we can do
						@map[obj].set_initial(prop, obj.initial_value(prop.to_sym))
						
						resimulate
					end
				end
				
				obj.signal_connect('range_changed') do |obj, prop|
					# do not handle modified because we don't care about the range
					@handlemodified = false
				end
			end
		end
		
		def clear_network
			@network = nil
			@map = {}
		end
		
		def do_modified
			if @handlemodified
				# rebuild network next time
				@network = nil
				@map = {}
			end
			
			@handlemodified = false
		end
		
		alias :orig_root :root=
		
		def root=(r)
			orig_root(r)
						
			if !@objectaddedsignal && Application.instance
				@objectaddedsignal = Application.instance.grid.signal_connect('object_added') do |grid, obj|
					do_object_added(obj)
				end
			
				@modifiedsignal = Application.instance.grid.signal_connect('modified') do |grid|
					do_modified
				end
			end
			
			clear_network
		end
		
		def build_network
			res = []
			map = {}
			
			# flatten objects
			@root.each do |obj|
				res += FlatFormat.flatten(map, obj)
			end
			
			# create new network
			@network = CCpg::Network.new
			nmap = {}
			
			# first add all the states
			res.select { |x| !x.is_a?(FlatFormat::Link) }.each do |o|
				nmap[o] = state = @network.add_state(o.fullname)
				
				o.state.keys.each do |prop|
					state.add_property(prop.to_s, o.node.initial_value(prop).to_s, o.node.integrated?(prop) ? true : false)
				end
			end
			
			# then all the links
			res.select { |x| x.is_a?(FlatFormat::Link) }.each do |o|
				nmap[o] = link = @network.add_link(nmap[map[o.from]], nmap[map[o.to]])
				
				o.state.keys.each do |prop|
					link.add_property(prop.to_s, o.node.get_property(prop).to_s, false)
				end
				
				vars = o.node.act_on.to_s.split(/\s*,\s*/)
				eq = o.node.equation.to_s.split(/\s*,\s*/, vars.length)
				
				eq.each_with_index do |e, idx|
					link.add_action(vars[idx], e)
				end
			end
			
			# make sure to complete the mapping from real obj -> cobj
			@map = {}
			map.each do |k, v|
				@map[k] = nmap[v]
			end
			
			if !@network.compile
				@network = nil
				@map = {}
				
				Application.instance.how_message(Gtk::Stock::DIALOG_ERROR, "<b>Compilation of network failed</b>", "<i>This probably means that your network contains invalid expressions</i>")
			end
			
			# reinstate monitors
			@monitors.each do |obj, properties|
				properties.each do |property, values|
					@network.set_monitor(@map[obj], property.to_s)
				end
			end
		end
		
		def update_values
			# propagate changed values back to ruby side
			# TODO
		end
		
		alias :orig_reset :reset
		
		def reset
			orig_reset
			
			@network.reset if @network
		end
		
		def resimulate
			return unless @range and !@monitors.empty?
			
			reset
			simulate_period(*@range)
		end

		def simulate_period(*args)
			return false if running?
			
			if !@network
				build_network
				
				return unless @network
			end
			
			@simulation_source = true
			
			from, ts, to = parse_period_range(*args)
			@range = [from, ts, to]

			steps = ((to - from) / ts.to_f).to_i
			signal_emit('period_start', from, ts, to)
			
			# run the actual simulation
			@network.run(from, ts, to)
			
			update_values

			signal_emit('period_stop')			
			@simulation_source = nil
		end
		
		alias :orig_set_monitor :set_monitor
		alias :orig_unset_monitor :unset_monitor
		alias :orig_monitor_data :monitor_data
		alias :orig_monitor_data_resampled :monitor_data_resampled

		def set_monitor(obj, prop)
			orig_set_monitor(obj, prop)
			
			if @network && @map[obj]
				@network.set_monitor(@map[obj], prop.to_s)
			end
		end
		
		def unset_monitor(obj, prop)
			orig_unset_monitor(obj, prop)
			
			if @network && @map[obj]
				@network.unset_monitor(@map[obj], prop.to_s)
			end
		end
		
		def monitor_data(obj, prop)
			if !@network || !@map[obj]
				return orig_monitor_data(obj, prop)
			end
			
			data = @network.monitor_data(@map[obj], prop.to_s)
			data
		end
		
		def monitor_data_resampled(obj, prop, to)
			if !@network || !@map[obj]
				return orig_monitor_data_resampled(obj, prop, to)
			end
			
			data = @network.monitor_data_resampled(@map[obj], prop.to_s, to)
			data
		end
	end
end
